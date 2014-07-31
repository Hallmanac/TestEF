use [TestEf]
select * from [Emails]
where [EmailAddress] = 'Brian_0000@Hallmanac.com'

use [TestEf]
select * from [Users]
where [FirstName] = 'Brian0000'

select * from [PhoneNumbers]
where [FormattedNumber] in ('(407)-616-9618', '(407)-616-9619')
order by [Id]

select * from [UserPhoneNumber]
order by [UserId]

--FormattedNumber
--(407)-616-9618
--(407)-616-9619